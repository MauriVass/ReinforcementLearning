import string
import cherrypy
import json
import numpy as np
import tensorflow as tf

class Network():
	def __init__(self):
		self.actions = 4
		self.num_outputs = self.actions + 4
		self.batchsize = 64
		self.discount_rate = 0.75
		
		self.experience = {'s': [], 'a': [], 'r': [], 'ns': [], 'done': []}
		self.counter = 0

		self.policyNetwork = self.ModelTemplate()
		self.policyNetwork.compile(optimizer='adam',
				loss=['mae'],
				metrics=['mae'])

		self.targetNetwork = self.ModelTemplate()
		self.copyNN()
		print(self.policyNetwork.summary())

	def ModelTemplate(self):
			model = tf.keras.Sequential([
							tf.keras.layers.Flatten(),
							tf.keras.layers.Dense(8, activation='relu'),
							tf.keras.layers.Dense(64, activation='relu'),
							tf.keras.layers.Dense(64, activation='relu'),
							tf.keras.layers.Dense(self.actions)
						])
			model.build(input_shape=(1,self.num_outputs))			
			return model

	def fit(self, exp):
		self.counter += 1
		if len(self.experience['s']) >= self.batchsize:
			for key in self.experience.keys():
				self.experience[key].pop(0)
		for key, value in exp.items():
			self.experience[key].append(value)
			
		if(self.counter==self.batchsize):
			self.counter=0

			states = np.asarray(self.experience['s'])
			actions = np.asarray(self.experience['a'])
			rewards = np.asarray(self.experience['r'])
			next_states = np.asarray(self.experience['ns'])
			dones = np.asarray(self.experience['done'])
			value_next = np.max(self.predictTN(next_states), axis=1)
			actual_values = np.where(dones, rewards, rewards+self.discount_rate*value_next)

			with tf.GradientTape() as tape:
				selected_action_values = tf.math.reduce_sum(
					self.predict(states) * tf.one_hot(actions, self.num_actions), axis=1)
				loss = tf.math.reduce_mean(tf.square(actual_values - selected_action_values))

			variables = self.policyNetwork.trainable_variables
			gradients = tape.gradient(loss, variables)
			self.optimizer.apply_gradients(zip(gradients, variables))

			return loss		

	def predictPN(self, input):
		input = tf.constant(input, shape=(1,self.num_outputs))
		output = self.policyNetwork.predict(input)
		return output[0]

	def predictTN(self, input):
		out = []
		for i in input:
			input = tf.constant(i, shape=(1,self.num_outputs))
			output = self.targetNetwork.predict(i)
			out.append(output[0])
		return out
	
	def copyNN(self):
		self.targetNetwork.set_weights(self.policyNetwork.get_weights())



class CalculatorWebService(object):
	#Required to be accessable online
	exposed=True

	def __init__(self):
		self.net = Network()

	def FIT(self):
		msg = cherrypy.request.body.readline().decode("utf-8")
		exp = self.decodeMsg(msg)
		self.net.fit(exp)

	def PREDICT_PN(self):
		msg = cherrypy.request.body.readline().decode("utf-8")
		input = np.array(msg.split('.')).astype(int)
		output = self.net.predictPN(input)
		out = '.'.join([str(o) for o in output])
		print('pn', out, np.shape(out))
		return out

	def PREDICT_TN(self):
		output = self.net.predictTN(values[0])
		out = '.'.join([str(o) for o in output])
		print('tn', out, np.shape(out))
		return out

	def COPY_NN(self):
		self.net.copyNN()

	def decodeMsg(self, msg):
		values = msg.split(',')
		#print(3, values, type(values), msg)
		currentstate = np.array(values[0].split('.')).astype(int)
		action = int(values[1])
		nextstate = np.array(values[2].split('.')).astype(int)
		reward = float(values[3])
		endstate = bool(values[4])

		exp = {'s': currentstate, 'a': action, 'r': reward, 'ns': nextstate, 'done': endstate}
		return exp

	# def GET(self,*path,**query):
	# 	output = 'Get'
	# 	return output

	# def POST(self,*path,**query):
	# 	msg = cherrypy.request.body.read()
	# 	print(msg)
	# 	output = 'Post'
	# 	return output

	# def PUT(self,*path,**query):
	# 	msg = cherrypy.request.body.read().decode("utf-8") 
	# 	msg = msg.split('.')
	# 	result = {'action':msg[4], 'value':[0.35,0]}
	# 	output = json.dumps(result)
	# 	return output

	# def DELETE(self,*path,**query):
	# 	output = 'Get'
	# 	return output

#'request.dispatch': cherrypy.dispatch.MethodDispatcher() => switch from default URL to HTTP compliant approch
conf = { '/': {	'request.dispatch': cherrypy.dispatch.MethodDispatcher(),
								'tools.sessions.on':True} 
					}
cherrypy.tree.mount(CalculatorWebService(), '/', conf)

cherrypy.config.update({'servet.socket_host':'0.0.0.0'})
cherrypy.config.update({'servet.socket_port':'8080'})

cherrypy.engine.start()
cherrypy.engine.block()
